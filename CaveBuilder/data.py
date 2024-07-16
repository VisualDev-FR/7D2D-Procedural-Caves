import json


class Vector3i:

    def __init__(self, value: str) -> None:
        self.x, self.y, self.z = value.split(",")


if __name__ == "__main__":

    overlapMargin = 30

    with open("datamining.txt", "r") as reader:
        datas = reader.read().split("\n")

    # {position}|{size}|{otherPos}|{otherSize}|{other.boundingBoxPosition}|{other.boundingBoxSize}|{other.rotation}");

    for data in datas:

        position, size, otherPos, otherSize, bbPos, bbSize, rotation = data.split("|")

        position = Vector3i(position)
        size = Vector3i(size)
        otherPos = Vector3i(otherPos)
        otherSize = Vector3i(otherSize)
        bbPos = Vector3i(bbPos)
        bbSize = Vector3i(bbSize)

        bool1 = position.x + size.x + overlapMargin < otherPos.x
        bool2 = otherPos.x + otherSize.x + overlapMargin < position.x

        print(f"{position.x + size.x + overlapMargin} < {otherPos.x}")

    print(len(datas))
